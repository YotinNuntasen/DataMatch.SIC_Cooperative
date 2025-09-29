
const personnelMap = new Map([

    //Sales Mapping To Same Format with NBO
  ['chen yi jhen', 'Chen Yi Jhen (Terry)'],
  ['intira loychoosak', 'Intira Loychoosak'],
  ['mr.shan sharma', 'Shan Sharma'],
  ['nuttapon b.', 'Nuttapon Boonto'],
  ['nattapon boonto', 'Nuttapon Boonto'],
  ['tunn prasoprat', 'Tunn Prasoprat'],
  ['lisha lee', 'Lisa'],
  ['koichiro', 'Koichiro Sasai'],
  
  //SellToCustomerName Mapping To Same Format with NBO
  ['mr. jonas honold', 'JH Electronic'],
  ['oregon rfid eu gmbh', 'Oregon RFID'],
  ['tempo communications,Inc.', 'tempocom'],
  ['tempo communications hungary kft', 'tempocom'],


]);


export function transformPersonnelInfo(name) {
  
  if (!name || typeof name !== 'string') {
    return name;
  }

  const cleanedName = name.trim().toLowerCase();

  
  if (personnelMap.has(cleanedName)) {
   
    return personnelMap.get(cleanedName);
  } else {
    console.warn(`[Personnel Mapping] Name "${name}" not found in mapping table. Returning original value.`);
    return name;
  }
}


export default personnelMap;
