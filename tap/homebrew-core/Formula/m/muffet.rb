class Muffet < Formula
  desc "Fast website link checker in Go"
  homepage "https://github.com/raviqqe/muffet"
  url "https://github.com/raviqqe/muffet/releases/download/v2.11.6/muffet_windows_amd64.zip"
  sha256 "3eedb4b575594afb0ce61f2cef98b1235f62feb4a969356d61b8648a1c0ad44f"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/raviqqe/muffet/archive/refs/tags/v2.11.6.tar.gz"
  end

  def install
    bin.install "muffet.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/muffet --version")

    expected = "failed to fetch root page: lookup does.not.exist"
    assert_match expected, shell_output("#{bin}/muffet https://does.not.exist 2>&1", 1)
  end
end
