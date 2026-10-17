class Libngspice < Formula
  desc "Spice circuit simulator as shared library"
  homepage "https://ngspice.sourceforge.io/"
  url "https://downloads.sourceforge.net/project/ngspice/ng-spice-rework/47/ngspice-47.tar.gz"
  sha256 "894e649651f1838a14095e5a5439e7d3aa63e87ede14d283173fda4fcdef675f"
  license :cannot_represent
  head "https://git.code.sf.net/p/ngspice/ngspice.git", branch: "master"

  livecheck do
    formula "ngspice"
  end

  bottle do
    sha256 cellar: :any, x86_64_windows: "203427ab8416d59d992ab5810b53941ae098a46316b555af212677f803b92157"
  end

  uses_from_macos "bison" => :build
  uses_from_macos "flex" => :build

  def install
    args = %w[
      --with-ngshared
      --enable-cider
      --disable-openmp
    ]

    system "./configure", *args, *std_configure_args
    system "make", "install"

    # remove script files
    rm_r(Dir[share/"ngspice/scripts"])
  end

  test do
    (testpath/"test.cpp").write <<~CPP
      #include <cstdlib>
      #include <ngspice/sharedspice.h>
      int ng_exit(int status, bool immediate, bool quitexit, int ident, void *userdata) {
        return status;
      }
      int main() {
        return ngSpice_Init(NULL, NULL, ng_exit, NULL, NULL, NULL, NULL);
      }
    CPP
    system ENV.cc, "test.cpp", "-I#{include}", "-L#{lib}", "-lngspice", "-o", "test"
    system "./test"
  end
end
