class Curlie < Formula
  desc "Power of curl, ease of use of httpie"
  homepage "https://rs.github.io/curlie/"
  url "https://github.com/rs/curlie/releases/download/v1.8.2/curlie_1.8.2_windows_amd64.zip"
  sha256 "c9c722de200518bdde66ac1211fc10633fc21448dea90cb1f16e4f485a47fb56"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/rs/curlie/archive/refs/tags/v1.8.2.tar.gz"
  end

  def install
    bin.install "curlie.exe"
  end

  test do
    assert_match "httpbin.org",
      shell_output("#{bin}/curlie -X GET httpbin.org/headers 2>&1")
  end
end
