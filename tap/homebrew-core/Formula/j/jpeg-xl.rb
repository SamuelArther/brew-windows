class JpegXl < Formula
  desc "New file format for still image compression"
  homepage "https://jpeg.org/jpegxl/index.html"
  url "https://github.com/libjxl/libjxl/releases/download/v0.12.0/jxl-x64-windows-static.7z"
  sha256 "ff147dc7ac4ce55392974ccc70f2a8a8ec0eff3ae28529b072258b66c8f01ab2"
  license "BSD-3-Clause"
  revision 1

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/libjxl/libjxl/archive/refs/tags/v0.12.0.tar.gz"
    regex(/^v?(\d+(?:\.\d+)+)$/i)
  end

  def install
    install_tree
    windows_exe "libexec/bin/benchmark_xl.exe", "benchmark_xl"
    windows_exe "libexec/bin/butteraugli_main.exe", "butteraugli_main"
    windows_exe "libexec/bin/cjxl.exe", "cjxl"
    windows_exe "libexec/bin/decode_and_encode.exe", "decode_and_encode"
    windows_exe "libexec/bin/display_to_hlg.exe", "display_to_hlg"
    windows_exe "libexec/bin/djxl.exe", "djxl"
    windows_exe "libexec/bin/djxl_fuzzer_corpus.exe", "djxl_fuzzer_corpus"
    windows_exe "libexec/bin/exr_to_pq.exe", "exr_to_pq"
    windows_exe "libexec/bin/generate_lut_template.exe", "generate_lut_template"
    windows_exe "libexec/bin/icc_simplify.exe", "icc_simplify"
    windows_exe "libexec/bin/jxl_from_tree.exe", "jxl_from_tree"
    windows_exe "libexec/bin/jxlinfo.exe", "jxlinfo"
    windows_exe "libexec/bin/jxltran.exe", "jxltran"
    windows_exe "libexec/bin/local_tone_map.exe", "local_tone_map"
    windows_exe "libexec/bin/pq_to_hlg.exe", "pq_to_hlg"
    windows_exe "libexec/bin/render_hlg.exe", "render_hlg"
    windows_exe "libexec/bin/ssimulacra2.exe", "ssimulacra2"
    windows_exe "libexec/bin/ssimulacra_main.exe", "ssimulacra_main"
    windows_exe "libexec/bin/texture_to_cube.exe", "texture_to_cube"
    windows_exe "libexec/bin/tone_map.exe", "tone_map"
    windows_exe "libexec/bin/xyb_range.exe", "xyb_range"
  end

  test do
    system bin/"benchmark_xl", "--help"
  end
end
