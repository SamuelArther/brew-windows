class WpCliCompletion < Formula
  desc "Bash completion for Wpcli"
  homepage "https://github.com/wp-cli/wp-cli"
  url "https://github.com/wp-cli/wp-cli/archive/refs/tags/v2.12.0.tar.gz"
  sha256 "5edf426895cad99c7fd6486de6618e7360ebcdbdda0684b78d587d67b4749345"
  license "MIT"
  head "https://github.com/wp-cli/wp-cli.git", branch: "main"

  bottle do
    root_url "https://github.com/SamuelArther/brew-windows/releases/download/wp-cli-completion-2.12.0"
    sha256 cellar: :any_skip_relocation, x86_64_windows: "f34efd67d0acb6a2fb9fb0bf4b99552d8257365a6ad019cd5b59f0b3ae07c257"
  end

  def install
    bash_completion.install "utils/wp-completion.bash" => "wp"
  end

  test do
    assert_match "-F _wp_complete",
      shell_output("bash -c 'source #{bash_completion}/wp && complete -p wp'")
  end
end
