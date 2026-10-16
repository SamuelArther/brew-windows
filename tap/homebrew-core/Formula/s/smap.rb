class Smap < Formula
  desc "Drop-in replacement for Nmap powered by shodan.io"
  homepage "https://github.com/s0md3v/Smap"
  url "https://github.com/s0md3v/Smap/archive/refs/tags/0.1.12.tar.gz"
  sha256 "870838dc01cbf2a018db8bbdee2ac439e4666e131d1f014843fc5b6994c33049"
  license "AGPL-3.0-or-later"
  head "https://github.com/s0md3v/Smap.git", branch: "main"

  livecheck do
    url :stable
    regex(/^v?(\d+(?:\.\d+)+)$/i)
  end

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "719f5f40247b333b7da631ae7fdec8085c54e0760641c41873892c5a73a43fa5"
  end

  depends_on "go" => :build

  allow_network_access! :test

  def fetch
    system "go", "mod", "download"
  end

  def install
    system "go", "build", *std_go_args, "./cmd/smap"
  end

  test do
    assert_match "scan report for google.com", shell_output("#{bin}/smap google.com p80,443")
    system bin/"smap", "google.com", "-oX", "output.xml"
    assert_path_exists testpath/"output.xml"
  end
end
