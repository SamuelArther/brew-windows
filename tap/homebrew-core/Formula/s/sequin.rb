class Sequin < Formula
  desc "Human-readable ANSI sequences"
  homepage "https://github.com/charmbracelet/sequin"
  url "https://github.com/charmbracelet/sequin/releases/download/v0.3.1/sequin_0.3.1_Windows_x86_64.zip"
  sha256 "7f10e402491cd5eecb5bb2bda74faea48b24d96f7c280819a827f5dcc47438e7"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/charmbracelet/sequin/archive/refs/tags/v0.3.1.tar.gz"
  end

  def install
    bin.install "sequin.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/sequin --version 2>&1")
  end
end
