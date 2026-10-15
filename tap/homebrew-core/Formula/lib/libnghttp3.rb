class Libnghttp3 < Formula
  desc "HTTP/3 library written in C"
  homepage "https://nghttp2.org/nghttp3/"
  url "https://repo.msys2.org/msys/x86_64/libnghttp3-1.18.0-1-x86_64.pkg.tar.zst"
  sha256 "45fdd8613fa1da08f6c0504377194497fea540ed1fec438bc557736459040669"
  license "MIT"

  # Prebuilt package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://github.com/ngtcp2/nghttp3/releases/download/v1.18.0/nghttp3-1.18.0.tar.xz"
  end

  def install
    install_tree
    install_library "libexec/usr"
  end

  test do
    assert_path_exists bin/"msys-nghttp3-9.dll"
  end
end
