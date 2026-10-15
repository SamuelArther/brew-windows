class Geckodriver < Formula
  desc "WebDriver <-> Marionette proxy"
  homepage "https://firefox-source-docs.mozilla.org/testing/geckodriver/"
  url "https://github.com/mozilla/geckodriver/releases/download/v0.37.1/geckodriver-v0.37.1-win64.zip"
  sha256 "dfed9315abe8d2fbc1b6161a2ee8002452e79cf05ee92fdc653a4e26bc35edd8"
  license "MPL-2.0"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/mozilla/geckodriver/archive/refs/tags/v0.37.1.tar.gz"
  end

  def install
    bin.install "geckodriver.exe"
  end

  test do
    test_port = free_port
    pid = spawn bin/"geckodriver", "--port", test_port.to_s
    sleep 2

    # A functional test requires Firefox so we just make sure HTTP GET has a response
    assert_equal "HTTP method not allowed", shell_output("curl -s http://localhost:#{test_port}/session")
  ensure
    Process.kill "TERM", pid
    Process.wait pid
  end
end
