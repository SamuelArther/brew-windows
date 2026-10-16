class Kanata < Formula
  desc "Cross-platform software keyboard remapper for Linux, macOS and Windows"
  homepage "https://github.com/jtroo/kanata"
  url "https://github.com/jtroo/kanata/archive/refs/tags/v1.12.0.tar.gz"
  sha256 "7081073d1d22fe4e404cf8e7d1dfa3f72562fb2d96538367c07f64877dcbf87a"
  license "LGPL-3.0-only"
  head "https://github.com/jtroo/kanata.git", branch: "main"

  bottle do
    sha256 cellar: :any, x86_64_windows: "6c9451f8fd357d414abb5c81e0e71f69cf02a96d6928b2ee579612a2da3b3fd9"
  end

  depends_on "rust" => :build

  deny_network_access!

  def fetch
    system "cargo", "fetch", *std_cargo_fetch_args
  end

  def install
    system "cargo", "install", *std_cargo_args
  end

  service do
    run [opt_bin/"kanata", "--no-wait", "--cfg", "#{Dir.home}/.config/kanata/kanata.kbd"]
    keep_alive true
    require_root true
    working_dir Dir.home
    environment_variables PATH: std_service_path_env
    log_path var/"log/kanata.log"
    error_log_path var/"log/kanata.log"
  end

  test do
    (testpath/"kanata.kbd").write <<~LISP
      (defsrc
        caps grv         i
                    j    k    l
        lsft rsft
      )

      (deflayer default
        @cap @grv        _
                    _    _    _
        _    _
      )

      (deflayer arrows
        _    _           up
                    left down rght
        _    _
      )

      (defalias
        cap (tap-hold-press 200 200 caps lctl)
        grv (tap-hold-press 200 200 grv (layer-toggle arrows))
      )
    LISP

    system bin/"kanata", "--check"
  end
end
