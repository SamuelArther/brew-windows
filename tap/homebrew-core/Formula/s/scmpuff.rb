class Scmpuff < Formula
  desc "Numeric file selection shortcuts for common git commands"
  homepage "https://github.com/mroth/scmpuff"
  url "https://github.com/mroth/scmpuff/releases/download/v0.7.0/scmpuff_0.7.0_windows_amd64.zip"
  sha256 "58c67cad9ced16b2aa5f62b9aacccf7463bdbb7c8dcceb3f2fb05424d00a1d09"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/mroth/scmpuff/archive/refs/tags/v0.7.0.tar.gz"
  end

  def install
    bin.install "scmpuff.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/scmpuff --version 2>&1")

    ENV["e1"] = "abc"
    assert_equal "abc", shell_output("#{bin}/scmpuff expand 1").strip
  end
end
