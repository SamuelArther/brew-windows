class Colortail < Formula
  desc "Like tail(1), but with various colors for specified output"
  homepage "https://github.com/joakim666/colortail"
  url "https://github.com/joakim666/colortail/releases/download/0.3.5/colortail-0.3.5.tar.gz"
  sha256 "8d259560c6f4a4aaf1f4bbdb2b62d3e1053e7e19fefad7de322131b7e80e294d"
  license "GPL-2.0-or-later"

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "8c8e608ac94d5a907980f370b39e7582a72d249ca84a9ae09a494bf40d1debdc"
  end

  def install
    system "./configure", *std_configure_args
    system "make", "install"
  end

  test do
    (testpath/"test.txt").write "Hello\nWorld!\n"
    assert_match(/World!/, shell_output("#{bin}/colortail -n 1 test.txt"))
  end
end
