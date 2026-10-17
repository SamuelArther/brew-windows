class Guichan < Formula
  desc "Small, efficient C++ GUI library designed for games"
  homepage "https://github.com/darkbitsorg/guichan"
  url "https://github.com/darkbitsorg/guichan/releases/download/v0.8.3/guichan-0.8.3.tar.gz"
  sha256 "2f3b265d1b243e30af9d87e918c71da6c67947978dcaa82a93cb838dbf93529b"
  license "BSD-3-Clause"

  bottle do
    root_url "https://github.com/SamuelArther/brew-windows/releases/download/guichan-0.8.3"
    sha256 cellar: :any, x86_64_windows: "cca1cf91467f8dc726c95ec6094361fb785426dba4dc75b52d9edf7a28bbcebb"
  end

  depends_on "pkgconf" => :test

  on_linux do
    depends_on "mesa"
    depends_on "mesa-glu"
  end

  def install
    system "./configure", *std_configure_args
    system "make", "install"
  end

  test do
    (testpath/"helloworld.cpp").write <<~CPP
      #include <guichan.hpp>

      int main(int argc, char **argv)
      {
          gcn::Gui gui;
          gcn::Container container;
          gui.setTop(&container);

          gui.logic();

          return 0;
      }
    CPP

    pkg_config_flags = shell_output("pkg-config --cflags --libs guichan-0.8").chomp.split
    system ENV.cxx, "helloworld.cpp", *pkg_config_flags, "-o", "helloworld"
    system "./helloworld"
  end
end
