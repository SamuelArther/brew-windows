class Elf2uf2Rs < Formula
  desc "Convert ELF files to UF2 for USB Flashing Bootloaders"
  homepage "https://github.com/JoNil/elf2uf2-rs"
  url "https://github.com/JoNil/elf2uf2-rs/archive/refs/tags/2.2.0.tar.gz"
  sha256 "7fd821623343bf6af63543a008caf49c1b7ad9f7e6df24403ae9c7a6bf325b54"
  license "0BSD"

  bottle do
    sha256 cellar: :any, x86_64_windows: "307cda6f5669955c0e2429b7aed98f77e2c0982a70ce20c417233305c8f9e121"
  end

  depends_on "rust" => :build

  on_linux do
    depends_on "pkgconf" => :build
    depends_on "systemd" # for libudev
  end

  deny_network_access!

  def fetch
    system "cargo", "fetch", *std_cargo_fetch_args
  end

  def install
    system "cargo", "install", *std_cargo_args
    (pkgshare/"examples").install Dir.glob("*.elf")
    (pkgshare/"examples").install Dir.glob("*.uf2")
  end

  test do
    system bin/"elf2uf2-rs", pkgshare/"examples/hello_usb.elf", "converted.uf2"
    assert compare_file pkgshare/"examples/hello_usb.uf2", testpath/"converted.uf2"
  end
end
