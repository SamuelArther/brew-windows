class SomagicTools < Formula
  desc "Tools to extract firmware from EasyCAP"
  homepage "https://code.google.com/archive/p/easycap-somagic-linux/"
  url "https://storage.googleapis.com/google-code-archive-downloads/v2/code.google.com/easycap-somagic-linux/somagic-easycap-tools_1.1.tar.gz"
  sha256 "b091723c55e6910cbf36c88f8d37a8d69856868691899683ec70c83b122a0715"
  license "GPL-2.0-or-later"
  compatibility_version 1

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "8e99bb79f4072f3b078be92449bf8a6292a5b253d74c0750bb63c61f2882f95d"
  end

  depends_on "libgcrypt"
  depends_on "libusb"

  deny_network_access!

  def install
    system "make"
    system "make", "PREFIX=#{prefix}", "install"
  end

  test do
    # requires actual firmware to function
    assert_match version.to_s, shell_output("#{bin}/somagic-extract-firmware --version 2>&1")
  end
end
