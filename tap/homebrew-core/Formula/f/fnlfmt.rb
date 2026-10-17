class Fnlfmt < Formula
  desc "Formatter for Fennel code"
  homepage "https://git.sr.ht/~technomancy/fnlfmt"
  url "https://git.sr.ht/~technomancy/fnlfmt/archive/0.4.0.tar.gz"
  sha256 "b9364b9e69aca6f7b272805a3338b599b1ff706ea881896960ab425f22b0800f"
  license "LGPL-3.0-or-later"

  bottle do
    sha256 cellar: :any, x86_64_windows: "0445bae657b01e3a47f85cce7a147b40c2a300696bed6712189250b09fae331c"
  end

  depends_on "lua"

  def install
    system "make"
    bin.install "fnlfmt"
  end

  test do
    (testpath/"testfile.fnl").write("(fn [abc def] nil)")
    expected = "(fn [abc def] nil)\n"
    assert_equal expected, shell_output("#{bin}/fnlfmt #{testpath}/testfile.fnl")
  end
end
