class Flawfinder < Formula
  desc "Examines code and reports possible security weaknesses"
  homepage "https://dwheeler.com/flawfinder/"
  url "https://dwheeler.com/flawfinder/flawfinder-2.0.20.tar.gz"
  sha256 "9d732a4e0fef1cd4eaeefd4a0093f183c5981f6c843711ceae6a63419404996b"
  license "GPL-2.0-or-later"
  head "https://github.com/david-a-wheeler/flawfinder.git", branch: "master"

  livecheck do
    url :homepage
    regex(/href=.*?flawfinder[._-]v?(\d+(?:\.\d+)+)\.t/i)
  end

  bottle do
    sha256 cellar: :any, x86_64_windows: "2abae007bb803d2e90ae9761a442d12d0d658753bf5d6fd04cf204b2eb28d551"
  end

  uses_from_macos "python"

  def install
    system "make", "prefix=#{prefix}", "install"
  end

  test do
    (testpath/"test.c").write <<~C
      int demo(char *a, char *b) {
        strcpy(a, "\n");
        strcpy(a, gettext("Hello there"));
      }
    C
    assert_match("Hits = 2\n", shell_output("#{bin}/flawfinder test.c"))
  end
end
