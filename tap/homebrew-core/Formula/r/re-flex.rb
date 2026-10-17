class ReFlex < Formula
  desc "Regex-centric, fast and flexible scanner generator for C++"
  homepage "https://www.genivia.com/doc/reflex/html"
  url "https://github.com/Genivia/RE-flex/archive/refs/tags/v6.5.0.tar.gz"
  sha256 "8497b36da92e28e03c63f7814ad3aebdddccc0b2afaba4579e054d253abe4cdf"
  license "BSD-3-Clause"

  bottle do
    sha256 cellar: :any, x86_64_windows: "ded2ff4f2f5abf7d678f0a759aed1880241e2c24985e16380eaacafd60f0c4c4"
  end

  depends_on "pcre2"

  conflicts_with "reflex", because: "both install `reflex` binaries"

  def install
    system "./configure", "--disable-dependency-tracking",
                          "--disable-silent-rules",
                          "--prefix=#{prefix}"
    system "make", "install"
  end

  test do
    (testpath/"echo.l").write <<~EOS
      %{
      #include <stdio.h>
      %}
      %option noyywrap main
      %%
      .+  ECHO;
      %%
    EOS
    system bin/"reflex", "--flex", "echo.l"
    assert_path_exists testpath/"lex.yy.cpp"
  end
end
