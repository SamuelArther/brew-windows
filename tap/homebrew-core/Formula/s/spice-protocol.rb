class SpiceProtocol < Formula
  desc "Headers for SPICE protocol"
  homepage "https://www.spice-space.org/"
  url "https://www.spice-space.org/download/releases/spice-protocol-0.14.5.tar.xz"
  sha256 "baf58449f6e89d19f475899ad5fb9196fdc46c03cc53233f4e39cf2978f9cff7"
  license "BSD-3-Clause"

  livecheck do
    url "https://www.spice-space.org/download/releases/"
    regex(/href=.*?spice-protocol[._-]v?(\d+(?:\.\d+)+)\.t/i)
  end

  bottle do
    sha256 cellar: :any, x86_64_windows: "3e7c8972deed3312c0b706e092fd62cf4dd2d90087a091f6ba69da433382ada4"
  end

  depends_on "meson" => :build
  depends_on "ninja" => :build

  def install
    system "meson", "setup", "build", *std_meson_args
    system "meson", "compile", "-C", "build", "--verbose"
    system "meson", "install", "-C", "build"
  end

  test do
    (testpath/"test.cpp").write <<~CPP
      #include <spice/protocol.h>
      int main() {
        return (SPICE_LINK_ERR_OK == 0) ? 0 : 1;
      }
    CPP

    system ENV.cc, "test.cpp", "-I#{include}/spice-1", "-o", "test"
    system "./test"
  end
end
