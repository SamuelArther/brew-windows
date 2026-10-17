class Tal < Formula
  desc "Align line endings if they match"
  homepage "https://thomasjensen.com/software/tal/"
  url "https://thomasjensen.com/software/tal/tal-1.9.tar.gz"
  mirror "https://www.mirrorservice.org/sites/download.salixos.org/x86_64/extra-14.2/source/misc/tal/tal-1.9.tar.gz"
  sha256 "5d450cee7162c6939811bca945eb475e771efe5bd6a08b520661d91a6165bb4c"
  license :public_domain

  livecheck do
    url :homepage
    regex(/href=.*?tal[._-]v?(\d+(?:\.\d+)+)\.t/i)
  end

  bottle do
    root_url "https://github.com/SamuelArther/brew-windows/releases/download/tal-1.9"
    sha256 cellar: :any_skip_relocation, x86_64_windows: "7e3dbb3edcb92159f67fc0097de287bb0ed8f5618a38cdbef49235de9a8808e3"
  end

  def install
    system "make", "tal"
    bin.install "tal"
    man1.install "tal.1"
  end

  test do
    (testpath/"test.c").write <<~C
      /***************************************************/
      /* some text and so on                    */
      /*       even more text                                   */
      /*       foo, bar. bar bar.                   */
      /***************************************************/
    C
    assert_equal <<~C, shell_output("#{bin}/tal -p 0 test.c")
      /***************************************************/
      /* some text and so on                             */
      /*       even more text                            */
      /*       foo, bar. bar bar.                        */
      /***************************************************/
    C
  end
end
