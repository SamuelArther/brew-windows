class DjangoCompletion < Formula
  desc "Bash completion for Django"
  homepage "https://www.djangoproject.com/"
  url "https://github.com/django/django/archive/refs/tags/6.1.2.tar.gz"
  sha256 "415e47f6da5a709ae0528335df435b413e7317ef9c99ab9eafc9614413edf1ad"
  license "BSD-3-Clause"
  head "https://github.com/django/django.git", branch: "main"

  livecheck do
    url :stable
    regex(/^v?(\d+(?:\.\d+)+)$/i)
  end

  bottle do
    root_url "https://github.com/SamuelArther/brew-windows/releases/download/django-completion-6.1.2"
    sha256 cellar: :any_skip_relocation, x86_64_windows: "04ce32f9eb68c33b2d1073e8dc3b11de8d545c5a8a0a70f21d20480c3751a598"
  end

  def install
    bash_completion.install "extras/django_bash_completion" => "django"
  end

  test do
    assert_match "-F _django_completion",
      shell_output("bash -c 'source #{bash_completion}/django && complete -p django-admin'")
  end
end
