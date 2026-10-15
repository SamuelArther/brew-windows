class Libclc < Formula
  desc "Implementation of the library requirements of the OpenCL C programming language"
  homepage "https://libclc.llvm.org/"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libclc-23.1.3-1-any.pkg.tar.zst"
  sha256 "fb0db0baff8f2872c873b69a26ce5e841cf07d7ccb1b07ff63bd90daf903fffb"
  license "Apache-2.0" => { with: "LLVM-exception" }

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://github.com/llvm/llvm-project/releases/download/llvmorg-23.1.3/llvm-project-23.1.3.src.tar.xz"
    regex(/^llvmorg[._-]v?(\d+(?:\.\d+)+)$/i)
  end

  def install
    install_tree
    install_library
  end

  test do
    assert_path_exists share/"clc/spirv-mesa3d-.spv"
  end
end
