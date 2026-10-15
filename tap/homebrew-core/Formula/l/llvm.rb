class Llvm < Formula
  desc "Next-gen compiler infrastructure"
  homepage "https://llvm.org/"
  version "23.1.3"
  url "https://github.com/llvm/llvm-project/releases/download/llvmorg-23.1.3/clang+llvm-23.1.3-x86_64-pc-windows-msvc.tar.xz"
  sha256 "9ee6a2192fbb30c776415af529927f76ca6b5d1d55699bb1315f9878e610b5b8"
  license "Apache-2.0" => { with: "LLVM-exception" }

  livecheck do
    url :stable
    regex(/^llvmorg[._-]v?(\d+(?:\.\d+)+)$/i)
    strategy :github_latest
  end

  # Official prebuilt LLVM/Clang toolchain for x86_64-pc-windows-msvc from llvm-project releases.
  # homebrew-core marks llvm keg-only only because macOS provides its own; Windows does not.
  # clang targets MSVC and needs the Visual Studio build tools / Windows SDK to link programs.

  def install
    install_tree
    %w[
      clang clang++ clang-cl clang-cpp clang-apply-replacements clang-check clang-doc
      clang-extdef-mapping clang-format clang-include-cleaner clang-include-fixer clang-installapi
      clang-linker-wrapper clang-move clang-offload-bundler clang-offload-packager clang-query
      clang-refactor clang-reorder-fields clang-repl clang-scan-deps clang-tblgen clang-tidy clangd
      diagtool dsymutil find-all-symbols ld.lld ld64.lld lld lld-link llc lli modularize opt pp-trace
      sancov sanstats wasm-ld
      llvm-addr2line llvm-ar llvm-as llvm-bcanalyzer llvm-bitcode-strip llvm-cat llvm-cfi-verify
      llvm-config llvm-cov llvm-cvtres llvm-cxxdump llvm-cxxfilt llvm-cxxmap llvm-debuginfod
      llvm-debuginfod-find llvm-diff llvm-dis llvm-dlltool llvm-dwarfdump llvm-dwarfutil llvm-dwp
      llvm-exegesis llvm-extract llvm-gsymutil llvm-ifs llvm-install-name-tool llvm-jitlink llvm-lib
      llvm-libtool-darwin llvm-link llvm-lipo llvm-lto llvm-lto2 llvm-mc llvm-mca llvm-ml llvm-ml64
      llvm-modextract llvm-mt llvm-nm llvm-objcopy llvm-objdump llvm-opt-report llvm-otool
      llvm-pdbutil llvm-profdata llvm-profgen llvm-ranlib llvm-rc llvm-readelf llvm-readobj
      llvm-readtapi llvm-reduce llvm-remarkutil llvm-rtdyld llvm-sim llvm-size llvm-split
      llvm-stress llvm-strings llvm-strip llvm-symbolizer llvm-tblgen llvm-tli-checker
      llvm-undname llvm-windres llvm-xray
    ].each do |cmd|
      windows_exe "libexec/bin/#{cmd}.exe", cmd
    end
    windows_cmd "libexec/bin/git-clang-format.bat", "git-clang-format"
  end

  test do
    assert_equal version.to_s, shell_output("#{bin}/llvm-config --version").strip
    (testpath/"test.c").write <<~C
      int main(void) { return 0; }
    C
    system bin/"clang", "-c", "test.c", "-o", "test.o"
    assert_path_exists testpath/"test.o"
  end
end
