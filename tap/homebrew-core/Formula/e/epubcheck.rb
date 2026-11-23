class Epubcheck < Formula
  desc "Validate EPUB files, version 2.0 and later"
  homepage "https://www.w3.org/publishing/epubcheck/"
  url "https://github.com/w3c/epubcheck/releases/download/v5.4.0/epubcheck-5.4.0.zip"
  sha256 "33350c61038e71dfb3d45a76aed04bf5481e6d5500cb780f6e98db8bbd15a28c"
  license "BSD-3-Clause"

  bottle do
    sha256 cellar: :any, x86_64_windows: "5ae2bac9e46f8a109f7922018ecb2e4a981c2fb444938e4474e0e818628f4f8e"
  end

  depends_on "openjdk"

  deny_network_access!

  def install
    jarname = "epubcheck.jar"
    libexec.install jarname, "lib"
    bin.write_jar_script libexec/jarname, "epubcheck"
  end

  test do
    assert_match "No errors or warnings detected", shell_output("#{bin}/epubcheck #{test_fixtures("test.epub")}")
  end
end
