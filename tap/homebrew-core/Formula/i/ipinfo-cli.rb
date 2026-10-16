class IpinfoCli < Formula
  desc "Official CLI for the IPinfo IP Address API"
  homepage "https://ipinfo.io/"
  url "https://github.com/ipinfo/cli/releases/download/ipinfo-3.3.2/ipinfo_3.3.2_windows_amd64.zip"
  version "3.3.2"
  sha256 "4dd90321cf023b941f8b53db4554cb83d8437879e890b9ecb2a7ca492b0556d3"
  license "Apache-2.0"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/ipinfo/cli/archive/refs/tags/ipinfo-3.3.2.tar.gz"
    regex(/^ipinfo[._-]v?(\d+(?:\.\d+)+)$/i)
  end

  def install
    bin.install "ipinfo.exe"
  end

  test do
    assert_equal version.to_s, shell_output("#{bin}/ipinfo version").chomp
    assert_equal "1.1.1.0\n1.1.1.1\n1.1.1.2\n1.1.1.3\n", `#{bin}/ipinfo prips 1.1.1.1/30`
  end
end
