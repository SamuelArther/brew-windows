class Aurora < Formula
  desc "Beanstalkd queue server console"
  homepage "https://xuri.me/aurora/"
  url "https://github.com/xuri/aurora/releases/download/2.2/aurora_windows_amd64_v2.2.zip"
  sha256 "5881a4f210c35283ded2031498e5dda96e403cdc8158e7413cfe33b908b37713"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/xuri/aurora/archive/refs/tags/2.2.tar.gz"
  end

  def install
    bin.install "aurora.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/aurora -v")
  end
end
