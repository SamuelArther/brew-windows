class Julia < Formula
  desc "Fast, Dynamic Programming Language"
  homepage "https://julialang.org/"
  url "https://julialang-s3.julialang.org/bin/winnt/x64/1.13/julia-1.13.1-win64.zip"
  sha256 "648e39c58158c5c8dde08bb0df0b5bce51d517be1396a37a539bb61bc241c8b4"
  license all_of: ["MIT", "BSD-3-Clause", "Apache-2.0", "BSL-1.0"]

  livecheck do
    url "https://julialang.org/downloads/"
    regex(/href=.*?julia[._-]v?(\d+(?:\.\d+)+)[._-]win64\.zip/i)
  end

  # Official Julia Windows x64 portable zip from julialang.org.

  def install
    install_tree
    windows_exe "libexec/bin/julia.exe", "julia"
  end

  test do
    assert_equal "4", shell_output("#{bin}/julia -E \"2 + 2\"").chomp
    assert_match version.to_s, shell_output("#{bin}/julia --version")
  end
end
