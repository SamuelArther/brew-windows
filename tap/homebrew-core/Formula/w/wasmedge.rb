class Wasmedge < Formula
  desc "Lightweight, high-performance, and extensible WebAssembly runtime"
  homepage "https://WasmEdge.org/"
  url "https://github.com/WasmEdge/WasmEdge/releases/download/0.18.0/WasmEdge-plugin-wasi_nn-ggml-0.18.0-windows_x86_64.zip"
  sha256 "a64b287fb859cb46398370581a98a594faf10453e859419a648a5544bf63f93c"
  license "Apache-2.0"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/WasmEdge/WasmEdge/releases/download/0.18.0/WasmEdge-0.18.0-src.tar.gz"
  end

  def install
    install_tree
    windows_exe "libexec/bin/wasmedge.exe", "wasmedge"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/wasmedge --version 2>&1")
  end
end
