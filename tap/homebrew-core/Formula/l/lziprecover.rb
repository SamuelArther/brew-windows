class Lziprecover < Formula
  desc "Data recovery tool and decompressor for files in the lzip compressed data format"
  homepage "https://www.nongnu.org/lzip/lziprecover.html"
  url "https://download.savannah.nongnu.org/releases/lzip/lziprecover/lziprecover-1.26.tar.gz"
  mirror "https://download-mirror.savannah.gnu.org/releases/lzip/lziprecover/lziprecover-1.26.tar.gz"
  sha256 "e234005a756d5649f41686116d3e548736d4a77e5a5ec37b943ca6650787801d"
  license "GPL-2.0-or-later"

  livecheck do
    url "https://download.savannah.gnu.org/releases/lzip/lziprecover/"
    regex(/href=.*?lziprecover[._-]v?(\d+(?:\.\d+)+)\.t/i)
  end

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "f1f8b2cf50a64253068b1ec52b402299cd53021e47b9abdfd42b009d6f00a2f5"
  end

  depends_on "lzip" => :test

  def install
    system "./configure", *std_configure_args
    system "make", "install"
  end

  test do
    filename = "data.txt"
    fixed_filename = "#{filename}_fixed"
    path = testpath/filename
    fixed_path = testpath/fixed_filename

    original_contents = "." * 1000
    path.write original_contents

    # Compress data into archive
    system formula_opt_bin("lzip")/"lzip", path
    refute_path_exists path

    # Corrupt the archive to test the recovery process
    File.open("#{path}.lz", "r+b") do |file|
      file.seek(7)
      data = file.read(1).unpack1("C*")
      data = ~data
      file.write([data].pack("C*"))
    end

    # Verify that file corruption is detected
    assert_match "Decoder error", shell_output("#{bin}/lziprecover -t #{path}.lz 2>&1", 2)

    # Attempt to recover the corrupted archive
    system bin/"lziprecover", "-B", "#{path}.lz"

    # Verify that recovered data is unchanged
    system bin/"lziprecover", "-d", "#{fixed_path}.lz"
    assert_equal original_contents, fixed_path.read
  end
end
