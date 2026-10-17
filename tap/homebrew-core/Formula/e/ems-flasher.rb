class EmsFlasher < Formula
  desc "Software for flashing the EMS Gameboy USB cart"
  homepage "https://lacklustre.net/projects/ems-flasher/"
  url "https://lacklustre.net/projects/ems-flasher/ems-flasher-0.03.tgz"
  sha256 "d77723a3956e00a9b8af9a3545ed2c55cd2653d65137e91b38523f7805316786"
  license "MIT"

  livecheck do
    url :homepage
    regex(/href=.*?ems-flasher[._-]v?(\d+(?:\.\d+)+)\.t/i)
  end

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "f303e0c3f1e6557c6cd86a5e974758ccf5bcbbcbcccf8c6a7703da7c026efc1a"
  end

  depends_on "pkgconf" => :build
  depends_on "libusb"

  def install
    system "make"
    bin.install "ems-flasher"
  end

  test do
    system bin/"ems-flasher", "--version"
  end
end
