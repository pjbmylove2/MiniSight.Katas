// Study 문서(공개용으로 가려진 복사본)를 블로그 복사용 웹 페이지 1개로 만든다.
// 사용: node Blog/build-blog.js <출력 html 경로>
// 새 단계를 올릴 때는 아래 SECTIONS의 enabled를 true로 바꾸고 다시 실행한다.
'use strict';

const fs = require('fs');
const path = require('path');

const REPO = path.resolve(__dirname, '..');
const STUDY = path.join(REPO, 'Study');
const TEMPLATE = path.join(__dirname, 'template.html');
const SANITIZER = path.join(REPO, '..', 'publish-sanitize.ps1');

// 글 끝에 붙는 "전체 코드" 링크의 GitHub 주소 (이 저장소의 main 브랜치)
const GITHUB_BLOB = 'https://github.com/pjbmylove2/MiniSight.Katas/blob/main/';

// codeDir: 이 저장소 안 Kata 코드 폴더 (Kata{글번호}*.cs를 찾는다, {id}는 글번호로 바뀐다)
// idFromFile: 글번호를 제목 대신 파일 이름에서 가져온다 (Day01.md → Day01)
// onlyDone: PROGRESS.md 표에서 최소분량이 ☑인 글만 올린다 (아직 안 한 Day 초안은 빼려고)
// seriesName / seriesLink: 글 끝 "시리즈 전체 보기" 링크 = 티스토리 카테고리 이름과 주소
const SECTIONS = [
    { id: 'basics', label: '1단계 · 초급', series: 'C# 기초', dir: 'Basics', pattern: /^B\d{2}-.*\.md$/, enabled: true,
      codeDir: 'Katas/Basics', seriesName: 'C# 기초 체력', seriesLink: '/category/C%23/C%23%20%EA%B8%B0%EC%B4%88%20%EC%B2%B4%EB%A0%A5' },
    { id: 'junior', label: '2단계 · 주니어', series: 'C# 주니어', dir: 'Days', pattern: /^Day\d{2}\.md$/, enabled: true,
      idFromFile: true, onlyDone: true, tag: 'C#주니어', codeDir: 'Katas/{id}', seriesName: 'C# 주니어' },
];

function findCode(sec, id) {
    if (!sec.codeDir) {
        return [];
    }
    const codeDir = sec.codeDir.replace('{id}', id);
    const dir = path.join(REPO, codeDir);
    if (!fs.existsSync(dir)) {
        return [];
    }
    return fs.readdirSync(dir)
        .filter(f => f.startsWith('Kata' + id) && f.endsWith('.cs'))
        .sort()
        .map(f => ({ name: f, url: GITHUB_BLOB + codeDir + '/' + f }));
}

function loadDoneIds() {
    // PROGRESS 표 줄 "| 날짜 | Day01 | ☑ | ..."에서 끝낸 글번호를 모은다
    const text = fs.readFileSync(path.join(STUDY, 'PROGRESS.md'), 'utf8');
    const done = new Set();
    const re = /^|[^|]*|s*(S+)s*|s*☑/gm;
    let m;
    while ((m = re.exec(text)) !== null) {
        done.add(m[1]);
    }
    return done;
}

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

function makeTitle(h1, series, fileId) {
    // "B04 · 이벤트 (`event` `+=`) · 25분" → "[C# 기초 B04] 이벤트 (event, +=)"
    const parts = h1.split(' · ');
    const id = fileId || parts[0].trim();
    let name = parts.slice(1).filter(p => !/^\d+분/.test(p.trim())).join(' · ').trim();
    name = name.replace(/`\s+`/g, ', ').replace(/`/g, '');
    return '[' + series + ' ' + id + '] ' + name;
}

function makeTags(h1, tag) {
    const tags = ['C#', '닷넷', tag || 'C#기초', '.NETFramework'];
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
    const doneIds = loadDoneIds();
    const groups = [];

    for (const sec of SECTIONS) {
        if (!sec.enabled) {
            continue;
        }
        const dir = path.join(STUDY, sec.dir);
        const files = fs.readdirSync(dir)
            .filter(f => sec.pattern.test(f))
            .filter(f => !sec.onlyDone || doneIds.has(f.replace(/.md$/, '')))
            .sort();
        const posts = files.map(f => {
            const raw = fs.readFileSync(path.join(dir, f), 'utf8').replace(/^﻿/, '');
            const h1 = raw.split(/\r?\n/)[0].replace(/^#\s+/, '');
            const fileId = sec.idFromFile ? f.replace(/.md$/, '') : '';
            const post = {
                id: fileId || h1.split(' · ')[0].trim(),
                file: sec.dir + '/' + f,
                title: makeTitle(h1, sec.series, fileId),
                tags: makeTags(h1, sec.tag),
                md: cleanBody(raw),
                code: [],
                seriesName: sec.seriesName || '',
                seriesLink: sec.seriesLink || '',
            };
            post.code = findCode(sec, post.id);
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
