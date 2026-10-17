class Polyml < Formula
  desc "Standard ML implementation"
  homepage "https://www.polyml.org/"
  url "https://github.com/polyml/polyml/archive/refs/tags/v5.9.2.tar.gz"
  sha256 "5cf5f77767568c25cf880acc2d0a32ee3d399e935475ab1626e8192fc3b07390"
  license "LGPL-2.1-or-later"
  head "https://github.com/polyml/polyml.git", branch: "master"

  bottle do
    root_url "https://github.com/SamuelArther/brew-windows/releases/download/polyml-5.9.2"
    sha256 cellar: :any, x86_64_windows: "ff425ffb225e3c9d31fe256aca04d0f8e3b9d94d575a240a9f857144e8786a2c"
  end

  def install
    system "./configure", "--disable-silent-rules", *std_configure_args
    system "make"
    system "make", "install"
  end

  test do
    (testpath/"hello.ml").write <<~EOS
      let
        fun concatWithSpace(a,b) = a ^ " " ^ b
      in
        TextIO.print(concatWithSpace("Hello", "World"))
      end
    EOS
    assert_match "Hello World", shell_output("#{bin}/poly --script hello.ml")
  end
end
