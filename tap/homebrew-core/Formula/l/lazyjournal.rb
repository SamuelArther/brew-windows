class Lazyjournal < Formula
  desc "TUI for logs from journalctl, file system, Docker, Podman and Kubernetes pods"
  homepage "https://github.com/Lifailon/lazyjournal"
  url "https://github.com/Lifailon/lazyjournal/releases/download/0.8.6/lazyjournal-0.8.6-windows-amd64.exe"
  sha256 "24270caab92b8e8b30fd8e4d6cb78f98c6696e0b8fe8b299807260ad9ee4d785"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/Lifailon/lazyjournal/archive/refs/tags/0.8.6.tar.gz"
  end

  def install
    bin.install "lazyjournal-0.8.6-windows-amd64.exe" => "lazyjournal.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/lazyjournal --version 2>&1")
  end
end
