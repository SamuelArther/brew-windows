class TCompletion < Formula
  desc "Completion for CLI power tool for Twitter"
  homepage "https://sferik.github.io/t/"
  url "https://github.com/sferik/x-cli/archive/refs/tags/v5.0.0.tar.gz"
  sha256 "30685de7d87d385a1c74b6ef47732c8b5259fe50f434efd651757e5529cc2fe9"
  license "MIT"
  head "https://github.com/sferik/x-cli.git", branch: "main"

  bottle do
    root_url "https://github.com/SamuelArther/brew-windows/releases/download/t-completion-5.0.0"
    sha256 cellar: :any_skip_relocation, x86_64_windows: "246363f885d8f38c151d143ce6ec703ed1322d503adfce6ae4407e164a86ef59"
  end

  def install
    bash_completion.install "legacy/etc/t-completion.sh" => "t"
    zsh_completion.install "legacy/etc/t-completion.zsh" => "_t"
  end

  test do
    assert_match "-F _t",
      shell_output("bash -c 'source #{bash_completion}/t && complete -p t'")
  end
end
