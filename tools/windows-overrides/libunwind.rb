class Libunwind < Formula
  desc "C API for determining the call-chain of a program"
  homepage "https://llvm.org/docs/ExceptionHandling.html"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libunwind-23.1.3-1-any.pkg.tar.zst"
  version "23.1.3"
  sha256 "62156b01dd43cf41fe868b3c62fcfab93ed50da2a595ea73373675eb635ab6d4"
  license "Apache-2.0" => { with: "LLVM-exception" }

  # The nongnu libunwind has no Windows port. LLVM's libunwind implements the same local-unwinding
  # C API (<libunwind.h>: unw_getcontext, unw_init_local, unw_step, unw_get_reg, unw_get_proc_name)
  # on Windows' SEH unwind tables, and it is what LLVM-MinGW toolchains link by default.
  # Prebuilt UCRT64 package from MSYS2; headers, import library and DLL go in the keg, plus the
  # libunwind.pc that pkg-config users of the nongnu library look for.

  livecheck do
    url "https://github.com/llvm/llvm-project.git"
    regex(/^llvmorg[._-]v?(\d+(?:\.\d+)+)$/i)
  end

  def install
    install_tree
    install_library
    (lib/"pkgconfig/libunwind.pc").write <<~PC
      prefix=${pcfiledir}/../..
      libdir=${prefix}/lib
      includedir=${prefix}/include

      Name: libunwind
      Description: LLVM libunwind (local unwinding on Windows SEH tables)
      Version: #{version}
      Cflags: -I${includedir}
      Libs: -L${libdir} -lunwind
    PC
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the unwinding API and the libraries instead.
    header = (include/"libunwind.h").read
    %w[unw_getcontext unw_init_local unw_step unw_get_reg unw_get_proc_name].each do |sym|
      assert_match sym, header
    end
    assert_path_exists lib/"libunwind.dll.a"
    assert_match "-lunwind", (lib/"pkgconfig/libunwind.pc").read
  end
end
