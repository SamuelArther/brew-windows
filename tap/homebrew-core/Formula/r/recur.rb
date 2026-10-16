class Recur < Formula
  desc "Retry a command with exponential backoff and jitter"
  homepage "https://github.com/dbohdan/recur"
  url "https://github.com/dbohdan/recur/releases/download/v3.3.0/recur-v3.3.0-windows-amd64.exe"
  sha256 "6e03ddc8898ec852de498e46606cc576883c15184978f4a92e086264a81d627c"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/dbohdan/recur/archive/refs/tags/v3.3.0.tar.gz"
  end

  def install
    bin.install "recur-v3.3.0-windows-amd64.exe" => "recur.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/recur --version 2>&1")
  end
end
