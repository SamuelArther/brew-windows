# Upstream's only Windows build is the 32-bit jo-1.9.exe attached to the 1.9 release; it runs on x64 Windows.
class Jo < Formula
  desc "JSON output from a shell"
  homepage "https://github.com/jpmens/jo"
  url "https://github.com/jpmens/jo/releases/download/1.9/jo-1.9.exe"
  sha256 "1c038153946ad7611166d36a5f6ce57e7704d8b89c51a4864c4fc821116521b6"
  license all_of: ["GPL-2.0-or-later", "MIT"]

  livecheck do
    url :stable
    strategy :github_latest
  end

  def install
    bin.install "jo-1.9.exe" => "jo.exe"
  end

  test do
    assert_equal "{\"foo\":1,\"a\":false}", shell_output("#{bin}/jo foo=1 a=false").chomp
  end
end
