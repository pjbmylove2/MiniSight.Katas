// 티스토리에 올리기 전에 스킨을 로컬에서 보는 간이 미리보기.
// 치환자에 샘플 값을 넣어 홈(home.html)과 글(post.html) 화면을 만든다. 실제 티스토리 출력과 100% 같지는 않다.
// 사용: node Blog/skin/preview.js <출력 폴더>
'use strict';

const fs = require('fs');
const path = require('path');

const SKIN = path.join(__dirname, 'k-docs');
const out = path.resolve(process.argv[2] || path.join(__dirname, 'preview-out'));

function readVars() {
    const xml = fs.readFileSync(path.join(SKIN, 'index.xml'), 'utf8');
    const vars = {};
    const re = /<variable>[\s\S]*?<name>([^<]+)<\/name>[\s\S]*?<default>([^<]*)<\/default>[\s\S]*?<\/variable>/g;
    let m;
    while ((m = re.exec(xml)) !== null) {
        vars[m[1].trim()] = m[2].trim();
    }
    return vars;
}

const POSTS = [
    { title: '[C# 기초 B01] null 다루기와 짧은 기호 (?., ??, throw 식 nameof, =>)', cat: 'C#', date: '2026. 11. 3.', sum: 'Day02~04 코드에 계속 나온 짧은 기호 5개를 읽고 쓸 수 있다. 친구가 도시락을 안 가져왔을 수도 있다.' },
    { title: '복잡성은 숨기고(Facade), 규칙은 강제하며(FSM), 흐름은 지배하라(Orchestrator).', cat: 'C# Framework 만들기', date: '2026. 10. 21.', sum: 'ICamera, IPlcCommunication 인터페이스로 장비를 감싸고 상태 머신으로 순서를 강제한다.' },
    { title: 'CancellationTokenSource', cat: '쓰레드', date: '2026. 10. 21.', sum: '엄마의 확성기와 게임하는 아이. Thread.Abort 대신 협력적 취소를 쓰는 이유.' },
    { title: 'readonly', cat: 'C#', date: '2026. 10. 21.', sum: '네임펜으로 쓴 이름표. 선언할 때와 생성자에서만 값을 넣을 수 있다.' }
];

const CATEGORY = '<ul class="tt_category"><li><a href="#" class="link_tit">분류 전체보기 <span class="c_cnt">(115)</span></a>' +
    '<ul class="category_list">' +
    '<li><a href="#" class="link_item">머신비전(Machine Vision)</a><ul class="sub_category_list"><li><a href="#" class="link_sub_item">C# Framework 만들기</a></li></ul></li>' +
    '<li class="selected"><a href="#" class="link_item">C#</a><ul class="sub_category_list"><li><a href="#" class="link_sub_item">쓰레드</a></li><li><a href="#" class="link_sub_item">EffectiveC#</a></li></ul></li>' +
    '<li><a href="#" class="link_item">C++</a></li><li><a href="#" class="link_item">MFC</a></li></ul></li></ul>';

const ARTICLE = `
<h2>1. 오늘 목표</h2><p>Day02~04 코드에 계속 나온 <b>짧은 기호 5개</b>를 읽고 쓸 수 있다. <code>?.</code> 와 <code>??</code> 를 먼저 본다.</p>
<h2>2. 비유: 도시락 확인</h2><blockquote>있으면 열어 보고(<code>?.</code>), 없으면 매점 빵 먹어(<code>??</code>)라고 미리 정한다.</blockquote>
<h2>3. 키워드 표</h2>
<table><thead><tr><th>Day</th><th>코드</th><th>뜻</th></tr></thead><tbody>
<tr><td>Day04</td><td><code>_timer?.Change(...)</code></td><td>타이머가 있을 때만 멈춘다</td></tr>
<tr><td>Day04</td><td><code>frame?.Dispose(); mat?.Dispose();</code></td><td>아직 손에 남아 있으면 버린다 (이미 넘겼으면 null이라 건너뜀)</td></tr>
</tbody></table>
<h3>3-1. 긴 코드</h3>
<pre><code>public Student(string name)
{
    // name이 null이면 바로 예외. 아니면 _name에 저장
    _name = name ?? throw new ArgumentNullException(nameof(name));
}</code></pre>
<h2>4. 셀프 체크</h2><p>friend가 null일 때 <code>friend.Menu</code>는 NullReferenceException으로 넘어진다.</p>`;

function blocks(tpl, name, fn) {
    const re = new RegExp('<' + name + '>([\\s\\S]*?)</' + name + '>', 'g');
    return tpl.replace(re, (all, inner) => fn(inner));
}
const keep = (t, name) => blocks(t, name, inner => inner);
const drop = (t, name) => blocks(t, name, () => '');
const repeat = (t, name, items, fill) => blocks(t, name, inner => items.map(it => fill(inner, it)).join(''));

function fillVals(t, vals) {
    return t.replace(/\[##_([a-z0-9_-]+)_##\]/gi, (all, key) => (key in vals ? vals[key] : ''));
}

function render(page) {
    let t = fs.readFileSync(path.join(SKIN, 'skin.html'), 'utf8');
    const vars = readVars();

    // 스킨 옵션 (안에 또 다른 옵션 블록이 있을 수 있어서 바뀌지 않을 때까지 반복)
    let before;
    do {
        before = t;
        t = t.replace(/<s_if_var_([a-z0-9-]+)>([\s\S]*?)<\/s_if_var_\1>/g, (a, n, inner) => (vars[n] ? inner : ''));
        t = t.replace(/<s_not_var_([a-z0-9-]+)>([\s\S]*?)<\/s_not_var_\1>/g, (a, n, inner) => (vars[n] ? '' : inner));
    } while (t !== before);

    // 이 화면에 없는 블록을 먼저 지우고, 남은 블록의 태그를 벗긴다
    ['s_list', 's_article_protected', 's_notice_rep', 's_tag', 's_guest', 's_ad_div', 's_article_prev'].forEach(n => { t = drop(t, n); });
    ['s_t3', 's_sidebar', 's_sidebar_element', 's_search', 's_tag_label', 's_article_next', 's_article_related', 's_paging'].forEach(n => { t = keep(t, n); });

    t = repeat(t, 's_rctps_rep', POSTS, (inner, p) => fillVals(inner, { rctps_rep_link: '#', rctps_rep_title: p.title }));
    t = repeat(t, 's_random_tags', ['C#', 'null', 'WinForms', 'Thread', 'OpenCV', 'PLC'], (inner, n) => fillVals(inner, { tag_link: '#', tag_class: 'cloud3', tag_name: n }));
    t = repeat(t, 's_article_related_rep', POSTS.slice(1), (inner, p) => fillVals(inner, { article_related_rep_link: '#', article_related_rep_title: p.title }));
    t = repeat(t, 's_paging_rep', [1, 2, 3], (inner, n) => fillVals(inner, { paging_rep_link: 'href="#"' + (n === 1 ? ' class="selected"' : ''), paging_rep_link_num: String(n) }));

    if (page === 'home') {
        t = repeat(t, 's_article_rep', POSTS, (inner, p) => fillVals(drop(keep(inner, 's_index_article_rep'), 's_permalink_article_rep'), {
            article_rep_link: '#', article_rep_title: p.title, article_rep_category: p.cat, article_rep_category_link: '#',
            article_rep_summary: p.sum, article_rep_simple_date: p.date
        }));
    } else {
        const p = POSTS[0];
        t = blocks(t, 's_article_rep', inner => fillVals(drop(keep(inner, 's_permalink_article_rep'), 's_index_article_rep'), {
            article_rep_link: '#', article_rep_title: p.title, article_rep_category: p.cat, article_rep_category_link: '#',
            article_rep_simple_date: p.date, article_rep_desc: ARTICLE,
            tag_label_rep: '<a href="#">C#</a><a href="#">null</a><a href="#">nameof</a>',
            article_next_link: '#', article_next_title: '[C# 기초 B02] 부모·자식 클래스'
        }));
    }

    const globals = {
        body_id: page === 'home' ? 'tt-body-index' : 'tt-body-page',
        page_title: page === 'home' ? 'K-프로그래밍' : POSTS[0].title,
        title: 'K-프로그래밍', desc: 'FA 비전 검사 현장의 C# 기록', blog_link: '#',
        blog_menu: '<ul><li><a href="#">홈</a></li><li><a href="#">시리즈</a></li><li><a href="#">포트폴리오</a></li><li><a href="#">방명록</a></li></ul>',
        category_list: CATEGORY, search_name: 'search', search_text: '', search_onclick_submit: '',
        count_total: '12,345', count_today: '12', count_yesterday: '34',
        prev_page: 'href="#"', next_page: 'href="#"', no_more_prev: 'no-more-prev', no_more_next: '',
        revenue_list_upper: '<div style="border:1px dashed #aaa;height:90px;margin:0 0 16px;display:flex;align-items:center;justify-content:center;font-size:12px;color:#888">[revenue_list_upper 자리]</div>',
        revenue_list_lower: '',
        comment_group: '<div style="padding:16px;border:1px dashed #aaa;color:#888;font-size:13px">[댓글 영역: 티스토리가 채움]</div>',
        rss_url: '#', taglog_link: '#', guestbook_link: '#'
    };
    Object.keys(vars).forEach(k => { globals['var_' + k] = vars[k]; });
    return fillVals(t, globals);
}

fs.mkdirSync(path.join(out, 'images'), { recursive: true });
fs.copyFileSync(path.join(SKIN, 'style.css'), path.join(out, 'style.css'));
fs.copyFileSync(path.join(SKIN, 'images', 'script.js'), path.join(out, 'images', 'script.js'));
['home', 'post'].forEach(page => {
    const html = render(page);
    const left = html.match(/<\/?s_[a-z0-9_-]+>|\[##_[^\]]+_##\]/g);
    fs.writeFileSync(path.join(out, page + '.html'), html, 'utf8');
    console.log(page + '.html' + (left ? '  남은 치환자: ' + [...new Set(left)].join(' ') : '  (남은 치환자 없음)'));
});
