class Scooter < Formula
  desc "Interactive find and replace in the terminal"
  homepage "https://github.com/thomasschafer/scooter"
  url "https://github.com/thomasschafer/scooter/releases/download/v0.10.0/scooter-v0.10.0-x86_64-pc-windows-msvc.zip"
  sha256 "a721360286ddbe045bbe4efdf73f40a1d1f25d35bad26bf2cc49080d53a9f505"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/thomasschafer/scooter/archive/refs/tags/v0.10.0.tar.gz"
  end

  def install
    bin.install "scooter.exe"
  end

  test do
    # scooter is a TUI application
    assert_match "Interactive find and replace TUI.", shell_output("#{bin}/scooter -h")
  end
end
