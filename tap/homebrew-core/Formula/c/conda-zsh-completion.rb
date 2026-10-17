class CondaZshCompletion < Formula
  desc "Zsh completion for conda"
  homepage "https://github.com/conda-incubator/conda-zsh-completion"
  url "https://github.com/conda-incubator/conda-zsh-completion/archive/refs/tags/v0.11.tar.gz"
  sha256 "584d5817e276f01f5d789e01ba5c6688667b38d3c9f4ad2cd735a9901e27aa33"
  license "WTFPL"
  head "https://github.com/conda-incubator/conda-zsh-completion.git", branch: "main"

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "59e8f335a2b994812a17654df5e65be55689e6170ee972d9e619902142fdb2c2"
  end

  uses_from_macos "zsh" => :test

  def install
    zsh_completion.install "_conda"
  end

  test do
    assert_match(/^_conda \(\) \{/,
      shell_output("zsh -c 'fpath=(#{zsh_completion} $fpath); autoload _conda; which _conda'"))
  end
end
