class Apng2gif < Formula
  desc "Convert APNG animations into animated GIF format"
  homepage "https://apng2gif.sourceforge.net/"
  url "https://downloads.sourceforge.net/project/apng2gif/1.8/apng2gif-1.8-src.zip"
  sha256 "9a07e386017dc696573cd7bc7b46b2575c06da0bc68c3c4f1c24a4b39cdedd4d"
  license all_of: ["libpng-2.0", "Zlib"]

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "f9d1495acbd56ed94972f0192449a9e62092b4cc87e8ed4ee214d6040df5bae0"
  end

  depends_on "libpng"

  deny_network_access!

  def install
    system "make"
    bin.install "apng2gif"
  end

  test do
    cp test_fixtures("test.png"), testpath/"test.png"
    system bin/"apng2gif", testpath/"test.png"
    assert_path_exists testpath/"test.gif", "Failed to create test.gif"
  end
end
