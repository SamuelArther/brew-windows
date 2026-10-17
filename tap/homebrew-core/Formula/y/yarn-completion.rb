class YarnCompletion < Formula
  desc "Bash completion for Yarn"
  homepage "https://github.com/dsifford/yarn-completion"
  url "https://github.com/dsifford/yarn-completion/archive/refs/tags/v0.17.0.tar.gz"
  sha256 "cc9d86bd8d4c662833424f86f1f86cfa0516c0835874768d9cf84aaf79fb8b21"
  license "MIT"

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "86faef33d8080bf75296b6b4c10c46057bcc3c6b284c33c7f965e8c6b5ecb93d"
  end

  on_macos do
    depends_on "bash"
  end

  deny_network_access!

  def install
    bash_completion.install "yarn-completion.bash" => "yarn"
  end

  test do
    output = shell_output("bash -c 'source #{bash_completion}/yarn && complete -p yarn'")
    assert_match "complete -F _yarn yarn", output
  end
end
