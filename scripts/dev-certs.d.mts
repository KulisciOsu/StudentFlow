/** Cesty k exportovanému vývojovému certifikátu a informace, zda vznikl nově. */
export interface DevCertificate {
  certPath: string
  keyPath: string
  created: boolean
}

/** Zajistí vývojový HTTPS certifikát v .certs/ kořene repozitáře; viz dev-certs.mjs. */
export function ensureDevCertificate(root: string, options?: { force?: boolean }): DevCertificate
