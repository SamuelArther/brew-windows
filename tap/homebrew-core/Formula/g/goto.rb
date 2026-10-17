class Goto < Formula
  desc "Bash tool for navigation to aliased directories with auto-completion"
  homepage "https://iridakos.com/programming/2019/04/10/shell-navigation-with-autocomplete"
  url "https://github.com/iridakos/goto/archive/refs/tags/v2.0.0.tar.gz"
  sha256 "460fe3994455501b50b2f771f999ace77ade295122e90e959084047dbfb1f0dc"
  license "MIT"

  bottle do
    root_url "https://github.com/SamuelArther/brew-windows/releases/download/goto-2.0.0"
    sha256 cellar: :any_skip_relocation, x86_64_windows: "28f54fe0d07c9d8175bbc99ec1692f1df1db1dc8c785b05e34281398e13da907"
  end

  def install
    bash_completion.install "goto.sh"
  end

  test do
    assert_match "-F _complete_goto_bash",
      shell_output("bash -c 'source #{bash_completion}/goto.sh && complete -p goto'")
  end
end
