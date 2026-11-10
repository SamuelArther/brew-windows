class Polyml < Formula
  desc "Standard ML implementation"
  homepage "https://www.polyml.org/"
  url "https://github.com/polyml/polyml/archive/refs/tags/v5.9.2.tar.gz"
  sha256 "5cf5f77767568c25cf880acc2d0a32ee3d399e935475ab1626e8192fc3b07390"
  license "LGPL-2.1-or-later"
  head "https://github.com/polyml/polyml.git", branch: "master"

  deny_network_access!

  bottle do
    sha256 cellar: :any, x86_64_windows: "28c3d8275b6cc042c8b173a9e6f637efdf64eadf89d8c24627dd0bdbcbe5d9b7"
  end

  def install
    system "./configure", "--disable-silent-rules", *std_configure_args
    system "make"
    system "make", "install"
  end

  test do
    (testpath/"hello.ml").write <<~ML
      let
        fun concatWithSpace(a,b) = a ^ " " ^ b
      in
        TextIO.print(concatWithSpace("Hello", "World"))
      end
    ML
    assert_match "Hello World", shell_output("#{bin}/poly --script hello.ml")
  end
end
