class Sshs < Formula
  desc "Graphical command-line client for SSH"
  homepage "https://github.com/quantumsheep/sshs"
  url "https://github.com/quantumsheep/sshs/releases/download/4.8.0/sshs-windows-amd64.exe"
  sha256 "4e249f479adee078b411dab9f5455d0bfa24476c5f1a9e95c3627fb36af701e7"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/quantumsheep/sshs/archive/refs/tags/4.8.0.tar.gz"
  end

  def install
    bin.install "sshs-windows-amd64.exe" => "sshs.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/sshs --version 2>&1")
  end
end
