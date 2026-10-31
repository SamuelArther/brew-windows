class Pgdbf < Formula
  desc "Converter of XBase/FoxPro tables to PostgreSQL"
  homepage "https://github.com/kstrauser/pgdbf"
  url "https://downloads.sourceforge.net/project/pgdbf/pgdbf/0.6.2/pgdbf-0.6.2.tar.xz"
  sha256 "e46f75e9ac5f500bd12c4542b215ea09f4ebee638d41dcfd642be8e9769aa324"
  license "GPL-3.0-or-later"

  deny_network_access!

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "32438142b4c1b1b8771a924e47baf257bdf5cd2368c0f33ec724e52ad7fed1e4"
  end

  def install
    args = []
    # Help old config scripts identify arm64 linux
    args << "--build=aarch64-unknown-linux-gnu" if OS.linux? && Hardware::CPU.arm64?

    system "./configure", *args, *std_configure_args
    system "make", "install"
  end

  test do
    # Build a minimal dBase III table with a character and a numeric column
    fields = [["NAME", "C", 10, 0], ["AGE", "N", 3, 0]]
    header = [0x03, 124, 1, 1, 2, 32 + (32 * fields.length) + 1, 1 + 10 + 3].pack("CCCCVvv") + ("\0" * 20)
    fields.each do |name, type, len, dec|
      header += name.ljust(11, "\0") + type + [0, len, dec].pack("VCC") + ("\0" * 14)
    end
    header += "\r"
    records = " #{"Alice".ljust(10)} 42" + " #{"Bob".ljust(10)}  7"
    (testpath/"test.dbf").binwrite header + records + "\x1A"

    output = shell_output("#{bin}/pgdbf #{testpath}/test.dbf")
    assert_match "CREATE TABLE test (name VARCHAR(10), age NUMERIC(3));", output
    assert_match "\\COPY test FROM STDIN\nAlice\t42\nBob\t7\n\\.\n", output
  end
end
