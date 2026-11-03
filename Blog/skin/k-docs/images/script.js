/* K-docs 스킨 스크립트: 테마 선택, 기술 칩, 연재 진행 막대, 목차, 읽는 시간, 모바일 카테고리 */
(function () {
  'use strict';
  var root = document.documentElement;

  // 테마: 자동(OS 따라감) / 라이트 / 다크. 선택은 이 브라우저에만 저장된다
  function currentTheme() {
    var t = null;
    try { t = localStorage.getItem('kp-theme'); } catch (e) { }
    return t || root.getAttribute('data-default-theme') || 'auto';
  }
  function applyTheme(t) {
    if (t === 'light' || t === 'dark') {
      root.setAttribute('data-theme', t);
    } else {
      root.removeAttribute('data-theme');
    }
    var buttons = document.querySelectorAll('[data-theme-pick]');
    for (var i = 0; i < buttons.length; i++) {
      buttons[i].setAttribute('aria-pressed', buttons[i].getAttribute('data-theme-pick') === t ? 'true' : 'false');
    }
  }
  applyTheme(currentTheme());
  var picks = document.querySelectorAll('[data-theme-pick]');
  for (var p = 0; p < picks.length; p++) {
    picks[p].addEventListener('click', function () {
      var t = this.getAttribute('data-theme-pick');
      try { localStorage.setItem('kp-theme', t); } catch (e) { }
      applyTheme(t);
    });
  }

  // 기술 칩: "C#, WinForms, ..." 글자를 칩으로 나눈다
  var skills = document.querySelector('.kp-skills');
  if (skills) {
    var names = (skills.getAttribute('data-skills') || '').split(',');
    for (var s = 0; s < names.length; s++) {
      var name = names[s].trim();
      if (name) {
        var chip = document.createElement('span');
        chip.textContent = name;
        skills.appendChild(chip);
      }
    }
  }

  // 연재 진행 막대: 현재 / 전체
  var now = document.querySelector('.kp-now');
  if (now) {
    var cur = parseFloat(now.getAttribute('data-current'));
    var total = parseFloat(now.getAttribute('data-total'));
    var fill = now.querySelector('.kp-bar-fill');
    if (fill && cur >= 0 && total > 0) {
      fill.style.width = Math.min(100, Math.round(cur / total * 100)) + '%';
    }
  }

  // 글 화면: 목차와 읽는 시간
  var article = document.querySelector('.kp-article');
  if (article) {
    var text = (article.textContent || '').replace(/\s+/g, '');
    var minutes = Math.max(1, Math.round(text.length / 500));
    var rt = document.querySelector('.kp-readtime');
    if (rt) {
      rt.textContent = '읽는 시간 ' + minutes + '분';
      rt.hidden = false;
    }

    var heads = article.querySelectorAll('h2, h3');
    var toc = document.querySelector('.kp-toc');
    var list = document.querySelector('.kp-toc-list');
    if (toc && list && heads.length >= 2) {
      var links = [];
      for (var h = 0; h < heads.length; h++) {
        var head = heads[h];
        if (!head.id) { head.id = 'kp-h-' + (h + 1); }
        var li = document.createElement('li');
        var a = document.createElement('a');
        a.href = '#' + head.id;
        a.textContent = head.textContent.trim();
        if (head.tagName === 'H3') { a.className = 'kp-toc-h3'; }
        li.appendChild(a);
        list.appendChild(li);
        links.push(a);
      }
      toc.hidden = false;

      // 지금 읽는 소제목을 목차에서 강조한다
      if ('IntersectionObserver' in window) {
        var io = new IntersectionObserver(function (entries) {
          for (var e = 0; e < entries.length; e++) {
            if (entries[e].isIntersecting) {
              var id = entries[e].target.id;
              for (var k = 0; k < links.length; k++) {
                links[k].classList.toggle('is-active', links[k].getAttribute('href') === '#' + id);
              }
            }
          }
        }, { rootMargin: '-80px 0px -70% 0px' });
        for (var o = 0; o < heads.length; o++) { io.observe(heads[o]); }
      }
    }
  }

  // 좁은 화면: 카테고리 열고 닫기
  var toggle = document.querySelector('.kp-side-toggle');
  var side = document.getElementById('kp-side');
  if (toggle && side) {
    toggle.addEventListener('click', function () {
      var open = side.classList.toggle('is-open');
      toggle.setAttribute('aria-expanded', open ? 'true' : 'false');
    });
  }
})();
