function isNullOrWhitespace(str) {
  return str == null || str.trim() === '';
}

(() => {

  const app = Stimulus.Application.start();

  app.register('form', class extends Stimulus.Controller {
    static get targets() {
      return [
        'owner',
        'repo',
        'base',
        'head',
        'live',
        'link',
        'linkAuthority',
        'linkOwner',
        'linkRepo',
        'linkBase',
        'linkHeadContainer',
        'linkHead'
      ];
    }

    initialize() {
      const location = window.location;

      this.data.set('initialPath', location.pathname);
      this.linkAuthorityTarget.innerText = location.origin;
    }

    connect() {
      this.update();
    }

    _replacePath(path) { window.history.replaceState({}, null, path); }

    // Yuck, there's probably a less terrible way to implement this.
    _updateBrowserUrl(owner, repo, base, head) {
      if (isNullOrWhitespace(owner)) {
        this._replacePath('/');
      }
      else {
        if (isNullOrWhitespace(repo)) {
          this._replacePath(`/${owner}`);
        } else {
          if (isNullOrWhitespace(base)) {
            this._replacePath(`/${owner}/${repo}`);
          } else {
            let path = `/${owner}/${repo}/compare/${base}`;
            if (!isNullOrWhitespace(head)) {
              path = `${path}...${head}`;
            }
            this._replacePath(path);
          }
        }
      }
    }

    _updateLinkPart(elem, value, placeholder) {
      if (isNullOrWhitespace(value)) {
        elem.classList.add('invalid');
        elem.innerText = placeholder;
      } else {
        elem.classList.remove('invalid');
        elem.innerText = value;
      }
    }

    _updateLink(owner, repo, base, head) {
      this._updateLinkPart(this.linkOwnerTarget, owner, ':owner:');
      this._updateLinkPart(this.linkRepoTarget, repo, ':repository:');
      this._updateLinkPart(this.linkBaseTarget, base, ':base:');

      if (isNullOrWhitespace(head)) {
        this.linkHeadContainerTarget.classList.add('hidden');
      } else {
        this.linkHeadContainerTarget.classList.remove('hidden');
        this.linkHeadTarget.innerText = head;
      }

      const anchor = this.linkTarget;

      if (isNullOrWhitespace(owner) || isNullOrWhitespace(repo) || isNullOrWhitespace(base)) {
        anchor.classList.add('invalid');
        anchor.classlist.setAttribute('href', '');
      } else {
        anchor.classList.remove('invalid');
        let path = `/${owner}/${repo}/compare/${base}`
        if (!isNullOrWhitespace(head)) {
          path = `${path}...${head}`;
        }
        anchor.setAttribute('href', path);
      }
    }

    updateLiveUrl() {
      const checkbox = this.liveTarget;
      if (checkbox.checked) {
        this.update();
      } else {
        this._replacePath(this.data.get('initialPath'));
      }
    }

    update() {
      const owner = this.ownerTarget.value;
      const repo = this.repoTarget.value;
      const base = this.baseTarget.value;
      const head = this.headTarget.value;
      const checkbox = this.liveTarget;

      // I'm not sure if I actually like this all that much, but I feel like it
      // could be valuable? Can't stick with an opinion so I'll make it
      // configurable for now.
      if (checkbox.checked) {
        this._updateBrowserUrl(owner, repo, base, head);
      }

      this._updateLink(owner, repo, base, head);
    }

    tryNavigateOnEnter(event) {
      if (event.key !== 'Enter') { return; }
      const location = this.linkTarget.getAttribute('href');
      if (!isNullOrWhitespace(location)) {
        Turbolinks.visit(location);
      }
    }
  });

  app.register('notes', class extends Stimulus.Controller {
    static get targets() {
      return ['secretInput'];
    }

    copyToClipboard() {
      const secretInput = this.secretInputTarget;
      secretInput.select();
      document.execCommand('copy');
    }
  });

})();
