class Briss < Formula
  desc "Crop PDF files"
  homepage "https://briss.sourceforge.net/"
  url "https://downloads.sourceforge.net/project/briss/release%200.9/briss-0.9.tar.gz"
  sha256 "45dd668a9ceb9cd59529a9fefe422a002ee1554a61be07e6fc8b3baf33d733d9"
  license "GPL-3.0-or-later"

  bottle do
    sha256 cellar: :any, x86_64_windows: "d37a368832ad3070035d484e18424ff727b0d47daa0bf4ec856c7e3c8b8bdf04"
  end

  depends_on "openjdk"

  def install
    libexec.install Dir["*.jar"]
    bin.write_jar_script libexec/"briss-#{version}.jar", "briss"
  end

  test do
    cp test_fixtures("test.pdf"), testpath
    system bin/"briss", "-s", "test.pdf", "-d", "output.pdf"
    assert_path_exists testpath/"output.pdf"
  end
end
