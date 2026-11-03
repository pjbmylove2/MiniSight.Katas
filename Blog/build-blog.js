// Study 문서(공개용으로 가려진 복사본)를 블로그 복사용 웹 페이지 1개로 만든다.
// 사용: node Blog/build-blog.js <출력 html 경로>
// 새 단계(주니어 Day 등)를 올릴 때는 아래 SECTIONS의 enabled를 true로 바꾸고 다시 실행한다.
'use strict';

const fs = require('fs');
const path = require('path');

const REPO = path.resolve(__dirname, '..');
const STUDY = path.join(REPO, 'Study');
const TEMPLATE = path.join(__dirname, 'template.html');
const SANITIZER = path.join(REPO, '..', 'publish-sanitize.ps1');

const SECTIONS = [
    { id: 'basics', label: '1단계 · 초급', series: 'C# 기초', dir: 'Basics', pattern: /^B\d{2}-.*\.md$/, enabled: true },
    { id: 'junior', label: '2단계 · 주니어', series: 'C# 주니어', dir: 'Days', pattern: /^Day\d{2}\.md$/, enabled: false },
];

function loadForbidden() {
    // 가리기 목록은 저장소 밖 publish-sanitize.ps1에만 있다. 이름을 이 파일에 적지 않으려고 거기서 읽는다.
    if (!fs.existsSync(SANITIZER)) {
        throw new Error('publish-sanitize.ps1을 찾지 못했다: ' + SANITIZER);
    }
    const text = fs.readFileSync(SANITIZER, 'utf8');
    const m = text.match(/\$forbidden\s*=\s*'([^']+)'/);
    if (!m) {
        throw new Error('publish-sanitize.ps1에서 $forbidden을 찾지 못했다');
    }
    return new RegExp(m[1].replace('(?i)', ''), 'i');
}

function makeTitle(h1, series) {
    // "B04 · 이벤트 (`event` `+=`) · 25분" → "[C# 기초 B04] 이벤트 (event, +=)"
    const parts = h1.split(' · ');
    const id = parts[0].trim();
    let name = parts.slice(1).filter(p => !/^\d+분/.test(p.trim())).join(' · ').trim();
    name = name.replace(/`\s+`/g, ', ').replace(/`/g, '');
    return '[' + series + ' ' + id + '] ' + name;
}

function makeTags(h1) {
    const tags = ['C#', '닷넷', 'C#기초', '.NETFramework'];
    const words = h1.match(/`([^`]+)`/g) || [];
    for (const w of words) {
        const t = w.replace(/`/g, '').replace(/<.*>/, '');
        if (/^[A-Za-z][A-Za-z.]*$/.test(t) && tags.indexOf(t) < 0) {
            tags.push(t);
        }
    }
    return tags;
}

function cleanBody(md) {
    let lines = md.replace(/\r\n/g, '\n').split('\n');
    lines = lines.slice(1);  // 제목(H1)은 블로그 제목 칸에 따로 넣는다

    // 개인 진행 체크 목록("## N. 체크")은 독자에게 필요 없어서 뺀다
    const out = [];
    let skipping = false;
    for (const line of lines) {
        if (/^## /.test(line)) {
            skipping = /^## \d+\.\s*체크\s*$/.test(line);
        }
        if (!skipping) {
            out.push(line);
        }
    }

    // Study 안의 상대 링크는 블로그에서 깨지므로 글자만 남긴다
    return out.join('\n')
        .replace(/\[([^\]]+)\]\((?!https?:)[^)]+\)/g, '$1')
        .trim() + '\n';
}

function build(outPath) {
    const forbidden = loadForbidden();
    const groups = [];

    for (const sec of SECTIONS) {
        if (!sec.enabled) {
            continue;
        }
        const dir = path.join(STUDY, sec.dir);
        const files = fs.readdirSync(dir).filter(f => sec.pattern.test(f)).sort();
        const posts = files.map(f => {
            const raw = fs.readFileSync(path.join(dir, f), 'utf8').replace(/^﻿/, '');
            const h1 = raw.split(/\r?\n/)[0].replace(/^#\s+/, '');
            const post = {
                id: h1.split(' · ')[0].trim(),
                file: sec.dir + '/' + f,
                title: makeTitle(h1, sec.series),
                tags: makeTags(h1),
                md: cleanBody(raw),
            };
            const hit = (post.title + '\n' + post.md).match(forbidden);
            if (hit) {
                throw new Error(f + '에 공개하면 안 되는 이름이 남아 있다: ' + hit[0]);
            }
            return post;
        });
        groups.push({ id: sec.id, label: sec.label, posts: posts });
    }

    const json = JSON.stringify(groups).replace(/</g, '\\u003c');
    const html = fs.readFileSync(TEMPLATE, 'utf8').replace('/*__POSTS__*/[]', json);
    fs.writeFileSync(outPath, html, 'utf8');

    const count = groups.reduce((n, g) => n + g.posts.length, 0);
    console.log('글 ' + count + '편 → ' + outPath);
}

if (process.argv.length < 3) {
    console.error('사용: node Blog/build-blog.js <출력 html 경로>');
    process.exit(1);
}
build(path.resolve(process.argv[2]));
