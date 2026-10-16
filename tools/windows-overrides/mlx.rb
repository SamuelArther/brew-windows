class Mlx < Formula
  desc "Array framework for machine learning (official Windows CPU build)"
  homepage "https://ml-explore.github.io/mlx/build/html/index.html"
  url "https://files.pythonhosted.org/packages/d0/c9/308e516701fecadb7b602a617cbff66cb1d3a543b6b8c205d9422a789f4f/mlx_cpu-0.32.4-py3-none-win_amd64.whl"
  version "0.32.4"
  sha256 "1631a01b3017c6ee846b8080d9b3fc762fd016dea15a843ee9b0c64cfda5b73f"
  license "MIT"

  # MLX's Metal backend is Apple-only, but MLX itself builds for Windows with its CPU backend
  # (OpenBLAS), and ml-explore publishes that build as the mlx-cpu wheel. The wheel carries the C++
  # library the Python package loads: mlx.dll and libopenblas.dll, the MSVC import library, the
  # headers and MLX's CMake package. This keg installs that library (find_package(MLX) with
  # CMAKE_PREFIX_PATH set to this prefix). The Python bindings are MSVC CPython extensions, so they
  # come from pip with python.org's Python: pip install "mlx[cpu]".

  livecheck do
    url "https://pypi.org/pypi/mlx-cpu/json"
    strategy :json do |json|
      json["info"]["version"]
    end
  end

  def install
    system "tar", "-xf", "mlx_cpu-#{version}-py3-none-win_amd64.whl"
    cd "mlx" do
      # The CMake package expects mlx.dll at the prefix root; keep DLLs in bin/ and resolve the
      # prefix through opt/ so the linked share/cmake/MLX works from HOMEBREW_PREFIX too.
      inreplace "share/cmake/MLX/MLXTargets-release.cmake", "${_IMPORT_PREFIX}/./mlx.dll", "${_IMPORT_PREFIX}/bin/mlx.dll"
      inreplace "share/cmake/MLX/MLXTargets.cmake", 'if(_IMPORT_PREFIX STREQUAL "/")',
                "set(_IMPORT_PREFIX \"#{opt_prefix.to_s.tr("\\", "/")}\")\nif(_IMPORT_PREFIX STREQUAL \"/\")"
      include.install "include/mlx"
      lib.install "lib/mlx.lib", "lib/libopenblas.lib"
      bin.install "mlx.dll", "libopenblas.dll"
      (share/"cmake").install "share/cmake/MLX"
    end
    doc.install "mlx_cpu-#{version}.dist-info/licenses/LICENSE"
  end

  def caveats
    <<~EOS
      mlx.dll is built with MSVC and needs the Microsoft Visual C++ Redistributable:
        winget install Microsoft.VCRedist.2015+.x64
      For the Python package use python.org's Python: pip install "mlx[cpu]"
    EOS
  end

  test do
    # No system C++ compiler on Windows to build homebrew-core's test program; check the build
    # files, then load mlx.dll and call mlx::core::version() through its exported symbol.
    assert_path_exists include/"mlx/mlx.h"
    assert_path_exists lib/"mlx.lib"
    assert_match "bin/mlx.dll", (share/"cmake/MLX/MLXTargets-release.cmake").read
    (testpath/"version.ps1").write <<~PS1
      $env:PATH = "#{bin};$env:PATH"
      Add-Type -TypeDefinition @"
      using System;
      using System.Runtime.InteropServices;
      public static class Mlx {
        [DllImport("mlx.dll", EntryPoint = "?version@core@mlx@@YAPEBDXZ")] static extern IntPtr version();
        public static string Version() { return Marshal.PtrToStringAnsi(version()); }
      }
      "@
      [Mlx]::Version()
    PS1
    assert_equal version.to_s,
                 shell_output("powershell -NoProfile -ExecutionPolicy Bypass -File #{testpath}/version.ps1").strip
  end
end
