class Xurls < Formula
  desc "Extract urls from text"
  homepage "https://github.com/mvdan/xurls"
  url "https://github.com/mvdan/xurls/releases/download/v2.6.0/xurls_v2.6.0_windows_amd64.exe"
  sha256 "053c6cc5eae94501006f49f83eb5fa2e30467703d630371a87b9b8001cfc9e23"
  license "BSD-3-Clause"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/mvdan/xurls/archive/refs/tags/v2.6.0.tar.gz"
  end

  def install
    bin.install "xurls_v2.6.0_windows_amd64.exe" => "xurls.exe"
  end

  test do
    output = pipe_output(bin/"xurls", "Brew test with https://brew.sh.")
    assert_equal "https://brew.sh", output.chomp

    output = pipe_output("#{bin}/xurls --fix", "Brew test with http://brew.sh.")
    assert_equal "Brew test with https://brew.sh/.", output.chomp
  end
end
