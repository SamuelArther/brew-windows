class Nerdlog < Formula
  desc "TUI log viewer with timeline histogram and no central server"
  homepage "https://dmitryfrank.com/projects/nerdlog/article"
  url "https://github.com/dimonomid/nerdlog/releases/download/v1.13.0/nerdlog_1.13.0_windows_amd64.zip"
  sha256 "499d5dad9620eca4106ede54f180f125194dbfc45b0a524474f42d356761126c"
  license "BSD-2-Clause"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/dimonomid/nerdlog/archive/refs/tags/v1.13.0.tar.gz"
  end

  def install
    bin.install "nerdlog.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/nerdlog --version 2>&1")
  end
end
