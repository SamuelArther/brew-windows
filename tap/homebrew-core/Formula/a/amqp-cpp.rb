class AmqpCpp < Formula
  desc "C++ library for communicating with a RabbitMQ message broker"
  homepage "https://github.com/CopernicaMarketingSoftware/AMQP-CPP"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-amqp-cpp-4.3.27-2-any.pkg.tar.zst"
  sha256 "e747b5f7e8c2422c50848843a1ce7c9f3eae83f53b290625b4b9ecb537a5cf1e"
  license "Apache-2.0"
  revision 1

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://github.com/CopernicaMarketingSoftware/AMQP-CPP/archive/refs/tags/v4.3.27.tar.gz"
    strategy :github_latest
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"amqpcpp.h"
    assert_path_exists lib/"libamqpcpp.dll.a"
    assert_path_exists bin/"libamqpcpp.dll"
    assert_match "Version: #{version}", (lib/"pkgconfig/amqpcpp.pc").read
  end
end
