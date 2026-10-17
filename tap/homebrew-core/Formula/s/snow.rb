class Snow < Formula
  desc "Whitespace steganography: coded messages using whitespace"
  homepage "https://darkside.com.au/snow/"
  url "https://darkside.com.au/snow/snow-20130616.tar.gz"
  mirror "https://cdn.netbsd.org/pub/pkgsrc/distfiles/snow-20130616.tar.gz"
  sha256 "c0b71aa74ed628d121f81b1cd4ae07c2842c41cfbdf639b50291fc527c213865"
  license "Apache-2.0"

  livecheck do
    url :homepage
    regex(/href=.*?snow[._-]v?(\d+(?:\.\d+)*)\.t/i)
  end

  bottle do
    root_url "https://github.com/SamuelArther/brew-windows/releases/download/snow-20130616"
    sha256 cellar: :any_skip_relocation, x86_64_windows: "a6a3285245e8a30915e93eaf3de50931ac204cdf9a8824f4c5edefc7744369f8"
  end

  conflicts_with "snowflake-cli", because: "both install `snow` binaries"

  def install
    # main.c:180:10: error: call to undeclared library function 'strcmp' with type 'int (const char *, const char *)'
    # main.c:180:10: note: include the header <string.h> or explicitly provide a declaration for 'strcmp'
    inreplace "main.c",
              "#include \"snow.h\"\n",
              "#include \"snow.h\"\n#include <string.h>\n"

    system "make"
    bin.install "snow"
    man1.install "snow.1"
  end

  test do
    touch "in.txt"
    touch "out.txt"
    system bin/"snow", "-C", "-m", "'Secrets Abound Here'", "-p",
           "'hello world'", "in.txt", "out.txt"
    # The below should get the response 'Secrets Abound Here' when testing.
    system bin/"snow", "-C", "-p", "'hello world'", "out.txt"
  end
end
