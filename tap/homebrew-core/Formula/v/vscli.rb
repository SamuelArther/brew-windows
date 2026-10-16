class Vscli < Formula
  desc "CLI/TUI that launches VSCode projects, with a focus on dev containers"
  homepage "https://github.com/michidk/vscli"
  url "https://github.com/michidk/vscli/releases/download/v1.3.3/vscli-x86_64-pc-windows-msvc.zip"
  sha256 "8927e9e8d3cca38692432caf4ab4dff373bc4c442b67a95eef7bcbe872119795"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/michidk/vscli/archive/refs/tags/v1.3.3.tar.gz"
  end

  def install
    bin.install "vscli.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/vscli --version 2>&1")
  end
end
