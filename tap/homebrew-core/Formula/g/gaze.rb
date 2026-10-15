class Gaze < Formula
  desc "Execute commands for you"
  homepage "https://github.com/wtetsu/gaze"
  url "https://github.com/wtetsu/gaze/releases/download/v1.2.1/gaze_windows_v1.2.1.zip"
  sha256 "59151c9f5a1d351199ab631d5f4d4f3f61edff946f258fd18b799a378336813b"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/wtetsu/gaze/archive/refs/tags/v1.2.1.tar.gz"
  end

  def install
    bin.install "gaze.exe"
  end

  test do
    pid = spawn bin/"gaze", "-c", "cp test.txt out.txt", "test.txt"
    sleep 5
    File.write("test.txt", "hello, world!")
    sleep 2
    Process.kill("TERM", pid)
    assert_match("hello, world!", File.read("out.txt"))
  end
end
