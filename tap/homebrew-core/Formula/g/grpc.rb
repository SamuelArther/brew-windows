class Grpc < Formula
  desc "Next generation open source RPC library and framework"
  homepage "https://grpc.io/"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-grpc-1.82.0-1-any.pkg.tar.zst"
  sha256 "0fff744dff314ce6e2cd24c0e9fb355ea4e584e4f56738379a38162168c436ce"
  license "Apache-2.0"

  # Prebuilt UCRT64 package from MSYS2; its runtime packages are pinned as resources.

  livecheck do
    url "https://github.com/grpc/grpc.git"
    regex(/^v?(\d+(?:\.\d+)+)$/i)
    strategy :github_releases
  end

  resource "mingw-w64-ucrt-x86_64-abseil-cpp" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-abseil-cpp-20260526.0-1-any.pkg.tar.zst"
    sha256 "89803dbb88b7e1a18169fd2ecefa2b85de3d99fcd5d6899b737e4451532b691f"
  end

  resource "mingw-w64-ucrt-x86_64-c-ares" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-c-ares-1.34.8-1-any.pkg.tar.zst"
    sha256 "2313d531bc9dc7aae6be3ce0f8ace516bb04191c4f3f62c78c7dfd246d2a9ba6"
  end

  resource "mingw-w64-ucrt-x86_64-cc-libs" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-cc-libs-16.2.0-6-any.pkg.tar.zst"
    sha256 "d8fff9e62443168bcda8ce746697243519c4f090ea607357ce94feac0bd10a50"
  end

  resource "mingw-w64-ucrt-x86_64-libgcc" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libgcc-16.2.0-6-any.pkg.tar.zst"
    sha256 "80b2497f019977dd9528b1abc8621b1328a0115ab1f724047ccbb56bf323f11d"
  end

  resource "mingw-w64-ucrt-x86_64-libstdc++" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libstdc++-16.2.0-6-any.pkg.tar.zst"
    sha256 "33c5e58a85a538cf843a8672991cce591f0231d66da3b66a57629712d64be17f"
  end

  resource "mingw-w64-ucrt-x86_64-libwinpthread" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libwinpthread-14.0.0.r426.g4564ee4b5-2-any.pkg.tar.zst"
    sha256 "a02f5e09d666d3be657c78c86fe67c975d04fe33b5211cd5ff1b23a91c118524"
  end

  resource "mingw-w64-ucrt-x86_64-openssl" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-openssl-3.6.5-1-any.pkg.tar.zst"
    sha256 "773e021ebad83f14b5c103db467411e8ff064663e1c7e7f730e2be72f4117064"
  end

  resource "mingw-w64-ucrt-x86_64-protobuf" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-protobuf-36.2-1-any.pkg.tar.zst"
    sha256 "39446eb547ad14187dc6f126ea978ec49c9648db340e14c244da033a030502eb"
  end

  resource "mingw-w64-ucrt-x86_64-re2" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-re2-20251105-3-any.pkg.tar.zst"
    sha256 "42b02745d0725a58a0bd442ebc7bf82b28b42257e28c5459bd02ee80b2a2fa92"
  end

  resource "mingw-w64-ucrt-x86_64-tzdata" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-tzdata-2026e-1-any.pkg.tar.zst"
    sha256 "f1809089f8ed5cec858ae8584289172896eb921be1e78ba583555f74e9015428"
  end

  resource "mingw-w64-ucrt-x86_64-zlib" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-zlib-1.3.2-2-any.pkg.tar.zst"
    sha256 "841401182976d2f9e17e5c0ebaac51f2a8014140ea53d67625e91c8fb3c85ea0"
  end

  def install
    msys2_install
    msys2_bin "ucrt64/bin/grpc_cpp_plugin.exe", "grpc_cpp_plugin"
    msys2_bin "ucrt64/bin/grpc_csharp_plugin.exe", "grpc_csharp_plugin"
    msys2_bin "ucrt64/bin/grpc_node_plugin.exe", "grpc_node_plugin"
    msys2_bin "ucrt64/bin/grpc_objective_c_plugin.exe", "grpc_objective_c_plugin"
    msys2_bin "ucrt64/bin/grpc_php_plugin.exe", "grpc_php_plugin"
    msys2_bin "ucrt64/bin/grpc_python_plugin.exe", "grpc_python_plugin"
    msys2_bin "ucrt64/bin/grpc_ruby_plugin.exe", "grpc_ruby_plugin"
  end

  test do
    assert_path_exists bin/"grpc_cpp_plugin.exe"
  end
end
