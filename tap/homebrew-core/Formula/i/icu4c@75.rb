class Icu4cAT75 < Formula
  desc "C/C++ and Java libraries for Unicode and globalization"
  homepage "https://icu.unicode.org/home"
  url "https://github.com/unicode-org/icu/releases/download/release-75-1/icu4c-75_1-src.tgz"
  version "75.1"
  sha256 "cb968df3e4d2e87e8b11c49a5d01c787bd13b9545280fc6642f826527618caef"
  license "Unicode-3.0"
  revision 1

  keg_only :versioned_formula

  # Disable date set 1 year after ICU 76.1 release
  disable! date: "2025-10-24", because: :versioned_formula

  def install
    odie "Major version bumps need a new formula!" if version.major.to_s != name[/@(\d+)$/, 1]

    args = %w[
      --disable-samples
      --disable-tests
      --enable-static
      --with-library-bits=64
    ]

    cd "source" do
      system "./configure", *args, *std_configure_args
      system "make"
      system "make", "install"
    end
  end

  test do
    if File.exist? "/usr/share/dict/words"
      system bin/"gendict", "--uchars", "/usr/share/dict/words", "dict"
    else
      (testpath/"hello").write "hello\nworld\n"
      system bin/"gendict", "--uchars", "hello", "dict"
    end
  end
end
