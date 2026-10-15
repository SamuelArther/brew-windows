class Bun < Formula
  desc "Incredibly fast JavaScript runtime, bundler, test runner, and package manager"
  homepage "https://bun.com/"
  url "https://github.com/oven-sh/bun/releases/download/bun-v1.4.3/bun-windows-x64.zip"
  version "1.4.3"
  sha256 "0bf84e808552beea67bf1202a287a35c6c825d837b287b3f8e9dd513dba35d3d"
  license all_of: [
    "MIT",
    "LGPL-2.0-or-later",
    "Apache-2.0",
    "BSD-2-Clause",
    "BSD-3-Clause",
    "IJG",
    "LGPL-2.1-or-later",
    "Zlib",
    "Apache-2.0" => { with: "LLVM-exception" },
  ]

  livecheck do
    url :stable
    regex(/^bun[._-]v?(\d+(?:\.\d+)+)$/i)
    strategy :github_latest
  end

  def install
    install_tree
    windows_exe "libexec/bun.exe", "bun"
    windows_exe "libexec/bun.exe", "bunx", "x"
  end

  test do
    (testpath/"hello.js").write <<~JS
      console.log("Hello world!");
    JS
    assert_equal "Hello world!\n", shell_output("#{bin}/bun run hello.js")
    assert_match version.to_s, shell_output("#{bin}/bun --version")
  end
end
